﻿namespace StrDss.Model.OrganizationDtos
{
    public class PlatformViewDto
    {
        public PlatformViewDto()
        {
            Subsidiaries = new List<PlatformViewDto>();
        }

        public long OrganizationId { get; set; }
        public string OrganizationType { get; set; } = null!;
        public string OrganizationCd { get; set; } = null!;
        public string OrganizationNm { get; set; } = null!;
        public DateTime UpdDtm { get; set; }
        public Guid? UpdUserGuid { get; set; }
        //TODO: Add FullName to Platformvw. Remove default value below after Leif Updates the view to include "Full Name".
        public string UpdUserNm { get; } = "Richard Anderson";

        //TODO: Add ManagingOrganizationID to PlatFormvw. Comment in line below when completed
        //public long ManagingOrganizationId { get; set; }
        public long? PrimaryNoticeOfTakedownContactId { get; set; }
        public string? PrimaryNoticeOfTakedownContactEmail { get; set; }
        public long? PrimaryTakedownRequestContactId { get; set; }
        public string? PrimaryTakedownRequestContactEmail { get; set; }
        public long? SecondaryNoticeOfTakedownContactId { get; set; }
        public string? SecondaryNoticeOfTakedownContactEmail { get; set; }
        public long? SecondaryTakedownRequestContactId { get; set; }
        public string? SecondaryTakedownRequestContactEmail { get; set; }
        public bool? IsActive { get; set; }
        public string? PlatformType { get; set; }
        public virtual ICollection<PlatformViewDto> Subsidiaries { get; set; }
    }
}