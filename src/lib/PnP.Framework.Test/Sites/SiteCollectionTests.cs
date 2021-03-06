﻿using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace PnP.Framework.Test.Sites
{
    //For those tests I had to manually update the generate mock files
    //I had to add two more requests after first and before second generate
    //I just debugged the third MockResponseProvider.GetResponse and send the body argument to the actual endpoint.
    [TestClass]
    public class SiteCollectionTests
    {
        private string communicationSiteGuid;
        private string teamSiteGuid;
        private string baseUrl;

        [TestInitialize]
        public void Initialize()
        {
            if (TestCommon.AppOnlyTesting())
            {
                Assert.Inconclusive("Test that require modern site collection creation are not supported in app-only.");
            }

            using (var clientContext = TestCommon.CreateTestClientContext())
            {
                communicationSiteGuid = Guid.NewGuid().ToString("N");
                teamSiteGuid = Guid.NewGuid().ToString("N");
                var baseUri = new Uri(clientContext.Url);
                baseUrl = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}";
            }
        }

        [TestCleanup]
        public void CleanUp()
        {
            if (!TestCommon.CurrentTestInIntegration)
            {
                //No need for cleanup
                return;
            }
            if (!TestCommon.AppOnlyTesting())
            {
                using (var clientContext = TestCommon.CreateTenantClientContext())
                {
                    var tenant = new Tenant(clientContext);

                    var communicationSiteUrl = $"{baseUrl}/sites/site{communicationSiteGuid}";
                    if (tenant.SiteExistsAnywhere(communicationSiteUrl) != SiteExistence.No)
                    {
#if !ONPREMISES
                        tenant.DeleteSiteCollection(communicationSiteUrl, false);
#else
                        tenant.DeleteSiteCollection(communicationSiteUrl);
#endif
                    }

                    var teamSiteUrl = $"{baseUrl}/sites/site{teamSiteGuid}";
                    if (tenant.SiteExistsAnywhere(teamSiteUrl) != SiteExistence.No)
                    {
#if !ONPREMISES
                        tenant.DeleteSiteCollection(teamSiteUrl, false);
#else
                        tenant.DeleteSiteCollection(teamSiteUrl);
#endif
                    }

                    // Commented this, first group cleanup needs to be implemented in this test case
                    //tenant.DeleteSiteCollection($"{baseUrl}/sites/site{teamSiteGuid}", false);
                    //TODO: Cleanup group
                }
            }
        }

        [TestMethod]
        public async Task CreateCommunicationSiteTestAsync()
        {

            using (var clientContext = TestCommon.CreateTestClientContext())
            {

                var commResults = await clientContext.CreateSiteAsync(new PnP.Framework.Sites.CommunicationSiteCollectionCreationInformation()
                {
                    Url = $"{baseUrl}/sites/site{communicationSiteGuid}",
                    SiteDesign = PnP.Framework.Sites.CommunicationSiteDesign.Blank,
                    Title = "Comm Site Test",
                    Lcid = 1033
                });

                Assert.IsNotNull(commResults);
            }
        }

        [TestMethod]
        public async Task CreateTeamNoGroupSiteTestAsync()
        {
            using (var clientContext = TestCommon.CreateTestClientContext())
            {

                var teamNoGroupSiteResult = await clientContext.CreateSiteAsync(new PnP.Framework.Sites.TeamNoGroupSiteCollectionCreationInformation()
                {
                    Url = $"{baseUrl}/sites/site{teamSiteGuid}",
                    Title = "Team no group Site Test",
                    Description = "Site description",
                    Lcid = 1033
                });

                Assert.IsNotNull(teamNoGroupSiteResult);
            }
        }


        //[TestMethod]
        //public async Task CreateTeamSiteTestAsync()
        //{
        //    using (var clientContext = TestCommon.CreateTestClientContext())
        //    {
        //        var teamResults = await clientContext.CreateSiteAsync(new Core.Sites.TeamSiteCollectionCreationInformation()
        //        {
        //            Alias = $"site{teamSiteGuid}",
        //            DisplayName = "Team Site Test",
        //        });
        //        Assert.IsNotNull(teamResults);
        //    }
        //}

        //[TestMethod]
        //public async Task GroupifyTeamSiteTestAsync()
        //{
        //    using (var clientContext = TestCommon.CreateTestClientContext("https://contoso.sharepoint.com/sites/groupify_me_2"))
        //    {

        //        clientContext.Load(clientContext.Web, p => p.Title, p => p.Description);
        //        clientContext.ExecuteQueryRetry();

        //        var teamResults = await clientContext.GroupifySiteAsync(new Core.Sites.TeamSiteCollectionGroupifyInformation()
        //        {
        //            Alias = $"groupify_me_2",
        //            DisplayName = clientContext.Web.Title,
        //            IsPublic = false,
        //            Description = clientContext.Web.Description,
        //        });

        //        Assert.IsNotNull(teamResults);
        //    }
        //}

    }
}
