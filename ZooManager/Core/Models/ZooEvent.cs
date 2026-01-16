using System;

namespace ZooManager.Core.Models;

public class ZooEvent
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Description { get; set; }
}