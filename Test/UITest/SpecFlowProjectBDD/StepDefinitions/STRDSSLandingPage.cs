﻿using UITest.PageObjects;
using UITest.TestDriver;
using TestFrameWork.Models;
using Configuration;
using NUnit.Framework.Legacy;
using OpenQA.Selenium;
using static SpecFlowProjectBDD.SFEnums;
using System.Reflection;
using SpecFlowProjectBDD.Helpers;

namespace SpecFlowProjectBDD.StepDefinitions
{
    [Binding]
    [Scope(Scenario = "STRDSSLandingPage")]
    public sealed class STRDSSLandingPage
    {
        private IDriver _Driver;
        private LandingPage _LandingPage;
        private DelistingWarningPage _DelistingWarningPage;
        private TermsAndConditionsPage _TermsAndConditionsPage;
        private PathFinderPage _PathFinderPage;
        private IDirLoginPage _IDRLoginPage;
        private NoticeOfTakeDownPage _NoticeOfTakeDownPage;
        private string _TestUserName;
        private string _TestPassword;
        private bool _ExpectedResult = false;
        private AppSettings _AppSettings;
        private SFEnums.UserTypeEnum _UserType;
        private BCIDPage _BCIDPage;

        public STRDSSLandingPage(SeleniumDriver Driver)
        {
            _Driver = Driver;
            _LandingPage = new LandingPage(_Driver);
            _DelistingWarningPage = new DelistingWarningPage(_Driver);
            _TermsAndConditionsPage = new TermsAndConditionsPage(Driver);
            _NoticeOfTakeDownPage = new NoticeOfTakeDownPage(_Driver);
            _PathFinderPage = new PathFinderPage(_Driver);
            _IDRLoginPage = new IDirLoginPage(_Driver);
            _BCIDPage = new BCIDPage(_Driver);
            _AppSettings = new AppSettings();
        }

        //User Authentication
        //       that I am an authenticated User "<UserName>" and the expected result is "<ExpectedResult>" and I am a "<UserType>" user
        [Given(@"that I am an authenticated User ""(.*)"" and the expected result is ""(.*)"" and I am a ""(.*)"" user")]
        public void GivenIAmAauthenticatedLGStaffMemberUser(string UserName, string ExpectedResult, string UserType)
        {
            _TestUserName = UserName;
            _TestPassword = _AppSettings.GetUser(_TestUserName) ?? string.Empty;
            _ExpectedResult = ExpectedResult.ToUpper() == "PASS" ? true : false;

            _Driver.Url = _AppSettings.GetServer("default");
            _Driver.Navigate();

            AuthHelper authHelper = new AuthHelper(_Driver);

            //Authenticate user using IDir or BCID depending on the user
            _UserType = authHelper.Authenticate(UserName, UserType);
            
            IWebElement TOC = null;

            try
            {
                TOC = _LandingPage.Driver.FindElement(Enums.FINDBY.CSSSELECTOR, TermsAndConditionsModel.TermsAndCondititionsCheckBox);
            }
            catch (NoSuchElementException ex)
            {
                //no Terms and Conditions. Continue
            }


            if ((null != TOC) && (TOC.Displayed))
            {
                //Nested Angular controls obscure the TermsAndConditionsCheckbox. Need JS 
                _TermsAndConditionsPage.TermsAndConditionsCheckBox.ExecuteJavaScript(@"document.querySelector(""body > app-root > app-layout > div.content > app-terms-and-conditions > p-card > div > div.p-card-body > div > div > div.checkbox-container > p-checkbox > div > div.p-checkbox-box"").click()");
                _TermsAndConditionsPage.ContinueButton.Click();
            }
        }

        //Landing Page for Government Users
        [When(@"I am an authenticated government user and I access the Data Sharing System landing page")]
        public void IAmAnAuthenticatedGovernmentUser()
        {
            //_UserType = SetUserType(UserType);

        }

        [Then("I should find where I can submit delisting warnings and requests to short-term rental platforms")]
        public void IShouldFindWhereICanSubmitDelistingWarningsAndRequests()
        {
            if (_UserType == UserTypeEnum.BCGOVERNMENT)
            {
                ClassicAssert.True(_LandingPage.SendNoticeButton.IsEnabled());
                ClassicAssert.True(_LandingPage.SendTakedownLetterButton.IsEnabled());
            }
        }

        //Landing Page for Platform Users:
        [When(@"I am an authenticated platform user ""(.*)"" and I access the Data Sharing System landing page")]
        public void WhenINavigateToTheDelistingWarningFeature(string UserType)
        {
            //_UserType = SetUserType(UserType);
        }


        [Then("I should find where I can upload a CSV file")]
        public void IShouldFindWhereICanUploadACSVDile()
        {
            if (_UserType == UserTypeEnum.PLATFORM)
            {
                ClassicAssert.True(_LandingPage.Upload_ListingsButton.IsEnabled());
            }
        }


        [Then("I should see some information about my obligations as a platform")]
        public void IShouldSeeSomeInformationAboutMyObligationsAsAPlatform()
        {
            if (_UserType == UserTypeEnum.PLATFORM)
            {
                ClassicAssert.True(_LandingPage.ViewPolicyGuidenceButton.IsEnabled());
            }
        }

        //Clear Navigation
        [When("I explore the landing page")]
        public void IExploreTheLandingPage()
        {
        }

        [Then("there should be a clear and intuitive navigation menu that guides me to other relevant sections of the application")]
        public void ThereShouldBeAClearAndIntuitiveNavigationMenu()
        {
        }

        //Brand Guidelines:
        [When("viewing the landing page")]
        public void ViewingThelandingPage()
        {
        }

        [Then("it should have visual elements consistent with branding guidelines")]
        public void ItShouldHaveVisualElementsConsistentWithBrandingGuidelines()
        {

        }

        /****************** Helper Methods **************************/

        //private UserTypeEnum SetUserType(string UserType)
        //{
        //    switch (UserType.ToUpper())
        //    {
        //        case "CODEENFOREMENTSTAFF":
        //        case "CODEENFORCEMENTADMIN":
        //        case "LOCALGOVERNMENTUSER":
        //            {
        //                _UserType = SFEnums.UserTypeEnum.BCGOVERNMENT;
        //                break;
        //            }
        //        case "PLATFORMUSER":
        //            {
        //                _UserType = SFEnums.UserTypeEnum.BCGOVERNMENT;
        //                break;
        //            }
        //        default:
        //            throw new ArgumentException("Unknown User Type (" + UserType + ")");
        //    }
        //    return (_UserType);
        //}
    }
}