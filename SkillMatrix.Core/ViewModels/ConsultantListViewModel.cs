// 📁 SkillMatrix.Core/ViewModels/ConsultantListViewModel.cs
using SkillMatrix.Core.DTOs;
using System.Collections.Generic;

namespace SkillMatrix.Core.ViewModels
{
    public class ConsultantListViewModel
    {
        public List<ConsultantListingDto> Consultants { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}