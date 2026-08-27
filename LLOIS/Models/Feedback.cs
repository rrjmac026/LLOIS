using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Models;

public enum FeedbackType { Bug, Concern, Suggestion }
public enum FeedbackStatus { Open, Resolved }

public class Feedback
{
    public int Id { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}