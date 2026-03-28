using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.Entities
{
    public class ExceptionLog
    {
        [Key]
        public int Id { get; set; }
        public string? ApplicationName { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
        public string? Source { get; set; }
        public string? MethodName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public string? AdditionalData { get; set; }
        public string? Request { get; set; }
    }
}
