using BookNest.Models;
using System.Collections.Generic;

namespace BookNest.ViewModels;

public class ReadingJourneyViewModel
{
    public IEnumerable<ReadingList> WantToRead { get; set; } = new List<ReadingList>();
    public IEnumerable<ReadingList> CurrentlyReading { get; set; } = new List<ReadingList>();
    public IEnumerable<ReadingList> Finished { get; set; } = new List<ReadingList>();
    public IEnumerable<ReadingStatus> Statuses { get; set; } = new List<ReadingStatus>();
}
