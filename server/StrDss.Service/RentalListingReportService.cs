﻿using AutoMapper;
using CsvHelper;
using CsvHelper.TypeConversion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StrDss.Common;
using StrDss.Data;
using StrDss.Data.Entities;
using StrDss.Data.Repositories;
using StrDss.Model;
using StrDss.Model.RentalReportDtos;
using StrDss.Service.CsvHelpers;
using System.Text;
using System.Text.RegularExpressions;

namespace StrDss.Service
{
    public interface IRentalListingReportService
    {
        Task<Dictionary<string, List<string>>> ValidateAndParseUploadFileAsync(string reportPeriod, long orgId, TextReader textReader);
        Task<Dictionary<string, List<string>>> CreateRentalListingReport(string reportPeriod, long orgId, Stream stream);
    }
    public class RentalListingReportService : ServiceBase, IRentalListingReportService
    {
        private IOrganizationRepository _orgRepo;
        private IRentalListingReportRepository _listingRepo;
        private IConfiguration _config;
        private ILogger<StrDssLogger> _logger;

        public RentalListingReportService(ICurrentUser currentUser, IFieldValidatorService validator, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor,
            IOrganizationRepository orgRepo, IRentalListingReportRepository listingRepo, IConfiguration config, ILogger<StrDssLogger> logger)
            : base(currentUser, validator, unitOfWork, mapper, httpContextAccessor, logger)
        {
            _orgRepo = orgRepo;
            _listingRepo = listingRepo;
            _config = config;
            _logger = logger;
        }

        public async Task<Dictionary<string, List<string>>> CreateRentalListingReport(string reportPeriod, long orgId, Stream stream)
        {
            byte[] sourceBin;
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                sourceBin = memoryStream.ToArray();
            }

            //check if any non printable byte exists?

            stream.Position = 0;
            using TextReader textReader = new StreamReader(stream, Encoding.UTF8);

            var errors = await ValidateAndParseUploadFileAsync(reportPeriod, orgId, textReader);

            if (errors.Count > 0) return errors;

            var entity = new DssRentalListingReport
            {
                IsProcessed = false,
                ReportPeriodYm = new DateOnly(Convert.ToInt32(reportPeriod.Substring(0, 4)), Convert.ToInt32(reportPeriod.Substring(5, 2)), 1),
                SourceBin = sourceBin,
                ProvidingOrganizationId = orgId,
            };

            await _listingRepo.AddRentalLisitngReportAsync(entity);
            
            _unitOfWork.Commit();

            return errors;
        }

        public async Task<Dictionary<string, List<string>>> ValidateAndParseUploadFileAsync(string reportPeriod, long orgId, TextReader textReader)
        {
            var errors = new Dictionary<string, List<string>>();

            var regex = RegexDefs.GetRegexInfo(RegexDefs.YearMonth);
            if (!Regex.IsMatch(reportPeriod, regex.Regex))
            {
                errors.AddItem("ReportPeriod", regex.ErrorMessage);
            }

            var platform = await _orgRepo.GetOrganizationByIdAsync(orgId);
            if (platform == null)
            {
                errors.AddItem("OrganizationId", $"Organization ID [{orgId}] doesn't exist.");
            }
            else if (platform.OrganizationType != OrganizationTypes.Platform)
            {
                errors.AddItem("OrganizationId", $"Organization type of the organization [{orgId}] is not {OrganizationTypes.Platform}.");
            }           

            if (errors.Count > 0)
            {
                return errors;
            }

            var csvConfig = CsvHelperUtils.GetConfig(errors, false);

            using var csv = new CsvReader(textReader, csvConfig);

            var mandatoryFields = new string[] { "rpt_period", "org_cd", "listing_id" };

            csv.Read();
            var headerExists = csv.ReadHeader();

            if (!headerExists)
            {
                errors.AddItem("File", "Header deosn't exist.");
                return errors;
            }

            if (!CheckCommonMandatoryFields(csv.HeaderRecord, mandatoryFields, errors))
            {
                return errors;
            }

            var reportPeriodMismatch = 0;
            var reportPeriodMissing = 0;
            var orgCdMissing = 0;
            var invalidOrgCds = new List<string>();
            var listingIdMissing = 0;
            var listingIds = new List<string>();
            var duplicateListingIds = new List<string>();

            var orgCds = new List<string>();

            while (csv.Read())
            {
                RentalListingRowUntyped row = null!;

                try
                {
                    row = csv.GetRecord<RentalListingRowUntyped>();

                    if (row.RptPeriod != reportPeriod) reportPeriodMismatch++;
                    if (row.RptPeriod.IsEmpty()) reportPeriodMissing++;
                    if (row.OrgCd.IsEmpty()) orgCdMissing++;
                    if (row.ListingId.IsEmpty()) listingIdMissing++;

                    if (row.OrgCd.IsNotEmpty() && !orgCds.Contains(row.OrgCd.ToUpper())) orgCds.Add(row.OrgCd.ToUpper());
                    if (!listingIds.Contains($"{row.OrgCd}-{row.ListingId.ToLower()}"))
                    {
                        listingIds.Add($"{row.OrgCd}-{row.ListingId.ToLower()}");
                    }
                    else
                    {
                        duplicateListingIds.Add($"{row.OrgCd}-{row.ListingId.ToLower()}");
                    }
                }
                catch (TypeConverterException ex)
                {
                    errors.AddItem(ex.MemberMapData.Member.Name, ex.Message);
                    break;
                }
                catch (CsvHelper.MissingFieldException)
                {
                    break; 
                }
                catch (CsvHelper.ReaderException ex)
                {
                    _logger.LogWarning(ex.Message);
                    throw;
                }
                catch (CsvHelperException ex)
                {
                    _logger.LogInformation(ex.ToString());
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    throw;
                }
            }

            var validOrgCds = await _orgRepo.GetManagingOrgCdsAsync(orgId);

            foreach (var org in orgCds)
            {
                if (!validOrgCds.Contains(org))
                {
                    invalidOrgCds.Add(org);
                }
            }

            if (reportPeriodMismatch > 0)
            {
                errors.AddItem("rpt_period", $"Report period mismatch found in {reportPeriodMismatch} record(s). The report period must be {reportPeriod}.");
            }

            if (reportPeriodMissing > 0)
            {
                errors.AddItem("rpt_period", $"Report period missing in {reportPeriodMissing} record(s). Please provide a report period.");
            }

            if (orgCdMissing > 0)
            {
                errors.AddItem("org_cd", $"Organization code missing in {orgCdMissing} record(s). Please provide an organization code.");
            }

            if (invalidOrgCds.Count > 0)
            {
                errors.AddItem("org_cd", $"Invalid organization code(s) found: {string.Join(", ", invalidOrgCds.ToArray())}. Please use one of the following valid organization code(s): {string.Join(", ", validOrgCds)}");
            }

            if (listingIdMissing > 0)
            {
                errors.AddItem("listing_id", $"Listing ID missing in {listingIdMissing} record(s). Please provide a listing ID.");
            }

            if (duplicateListingIds.Count > 0)
            {
                errors.AddItem("listing_id", $"Duplicate listing ID(s) found: {string.Join(", ", duplicateListingIds.ToArray())}. Each listing ID must be unique within an organization code.");
            }

            return errors;
        }

        private bool CheckCommonMandatoryFields(string[] headers, string[] mandatoryFields, Dictionary<string, List<string>> errors)
        {
            headers = CsvHelperUtils.GetLowercaseFieldsFromCsvHeaders(headers);

            foreach (var field in mandatoryFields)
            {
                if (!headers.Any(x => x.ToLower() == field))
                    errors.AddItem("File", $"Header [{field}] is missing");
            }

            if (errors.Count > 0)
                errors.AddItem("File", "Please ensure the file headers are correct");

            return errors.Count == 0;
        }
    }
}