using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SkillMatrix.Core.ViewModels;
using SkillMatrix.Data.Services;

namespace SkillMatrix.Web.Areas_Admin_Pages_Dashboard
{
    public class TimelineModel : PageModel
    {
        private readonly AdoNetService _adoNetService;

        public TimelineViewModel TimelineData { get; set; }

        public TimelineModel(AdoNetService adoNetService)
        {
            _adoNetService = adoNetService;
        }

        public async Task OnGetAsync()
        {
            TimelineData = await _adoNetService.GetTimelineDataAsync(6);
            DateTime current = TimelineData.StartDate;
            for (int i = 0; i < 6; i++)
            {
                TimelineData.Months.Add(new TimelineMonthDto {
                    Name = current.ToString("MMMM yyyy"),
                    DaysInMonth = DateTime.DaysInMonth(current.Year, current.Month)
                });
                current = current.AddMonths(1);
            }
            
        }
    }
}