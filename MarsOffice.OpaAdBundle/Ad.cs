using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarsOffice.Dto;
using MarsOffice.OpaAdBundle.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Graph;

namespace MarsOffice.Opa.AdBundle
{
    public class Ad
    {
        private readonly string _zerosGuid = Guid.Empty.ToString();
        private readonly GraphServiceClient _graphClient;

        public Ad(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        [FunctionName("Ad")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/ad/data")] HttpRequest req)
        {
            var applications = new List<ApplicationDto>();
            var appsRequest = _graphClient
                .Applications
                .Request()
                .Select(x => new { x.AppId, x.AppRoles, x.DisplayName });
            while (appsRequest != null)
            {
                var appsResponse = await appsRequest.GetAsync();
                applications.AddRange(appsResponse.Select(x => new ApplicationDto
                {
                    Id = x.AppId,
                    Name = x.DisplayName,
                    IsDisabled = false,
                    Roles = x.AppRoles?.Select(r => new RoleDto
                    {
                        Id = r.Id?.ToString(),
                        Name = r.DisplayName
                    }).ToList()
                }).ToList());
                appsRequest = appsResponse.NextPageRequest;
            }


            var users = new List<UserDto>();

            var usersRequest = _graphClient
                .Users
                .Request()
                .Expand(x => x.AppRoleAssignments)
                .Select(x => new { x.Id, x.Mail, x.GivenName, x.Surname, x.AccountEnabled, x.AppRoleAssignments });

            while (usersRequest != null)
            {
                var usersResponse = await usersRequest.GetAsync();
                foreach (var u in usersResponse)
                {
                    var dto = new UserDto
                    {
                        Email = u.Mail,
                        Id = u.Id,
                        FirstName = u.GivenName,
                        LastName = u.Surname,
                        IsDisabled = u.AccountEnabled == null || u.AccountEnabled == false,
                        RoleIds = u.AppRoleAssignments?.Where(x => x.AppRoleId != null && x.AppRoleId.Value.ToString() != _zerosGuid)
                            .Select(x => x.AppRoleId?.ToString()).Distinct().ToList()
                    };

                    users.Add(dto);
                }
                usersRequest = usersResponse.NextPageRequest;
            }


            var groups = new List<GroupDto>();
            var groupsRequest = _graphClient
                .Groups
                .Request()
                .Filter("mailEnabled eq false and securityEnabled eq true")
                .Expand(x => x.Members)
                .Select(x => new { x.Id, x.DisplayName, x.Members });
            while (groupsRequest != null)
            {
                var groupsResponse = await groupsRequest.GetAsync();
                foreach (var g in groupsResponse)
                {
                    var childrenGroupIds = g.Members?.Where(m => m.ODataType != null && m.ODataType.ToLower() == "#microsoft.graph.group")
                        .Select(x => x.Id).Distinct().ToList();
                    var childrenUserIds = g.Members?.Where(m => m.ODataType != null && m.ODataType.ToLower() == "#microsoft.graph.user")
                        .Select(x => x.Id).Distinct().ToList();
                    var group = new GroupDto
                    {
                        Id = g.Id,
                        Name = g.DisplayName,
                        ChildrenIds = childrenGroupIds
                    };
                    groups.Add(group);

                    if (childrenUserIds != null && childrenUserIds.Any())
                    {
                        var foundUsers = users.Where(x => childrenUserIds.Contains(x.Id)).ToList();
                        foreach (var foundUser in foundUsers)
                        {
                            if (foundUser.GroupIds == null)
                            {
                                foundUser.GroupIds = new List<string>();
                            }
                            var hs = new HashSet<string>(foundUser.GroupIds)
                            {
                                g.Id
                            };
                            foundUser.GroupIds = hs;
                        }
                    }
                }
                groupsRequest = groupsResponse.NextPageRequest;
            }

            foreach (var group in groups)
            {
                if (group.ChildrenIds == null || !group.ChildrenIds.Any())
                {
                    continue;
                }
                var childGroups = groups.Where(x => group.ChildrenIds.Contains(x.Id)).ToList();
                foreach (var cg in childGroups)
                {
                    cg.ParentId = group.Id;
                }
            }


            return new OkObjectResult(new AdBundleDto
            {
                Applications = applications,
                Groups = groups,
                Users = users
            });
        }
    }
}
