using MarsOffice.Dto;
using System;
using System.Collections.Generic;

namespace MarsOffice.OpaAdBundle.Abstractions
{
    public class AdBundleDto
    {
        public IEnumerable<ApplicationDto> Applications { get; set; }
        public IEnumerable<GroupDto> Groups { get; set; }
        public IEnumerable<UserDto> Users { get; set; }
    }
}
