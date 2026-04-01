namespace SkillMatrix.Core.ViewModels
{
    public class TimelineViewModel
    {
        public DateTime StartDate {get; set;}
        public int TotalDays{ get; set;}
        public List<TimelineMonthDto> Months {get; set;} = new();
        public List<ConsultantTimelineDto> Consultants {get; set;} = new();

    }

    public class TimelineMonthDto {
        public string Name { get; set; }
        public int DaysInMonth { get; set; }
    }

    public class ConsultantTimelineDto {
        public string NomComplet { get; set; }
        public List<MissionBarDto> MissionBars { get; set; } = new();
    }

    public class MissionBarDto {
        public string Label { get; set; } 
        public int StartColumn { get; set; } 
        public int ColumnSpan { get; set; }  
    }
}