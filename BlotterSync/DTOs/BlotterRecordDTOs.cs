using System;
using System.ComponentModel.DataAnnotations;

namespace BlotterSync.DTOs
{
    public class BlotterRecordDTOs
    {
        public class BlotterRecordDTO
        {
            public int RecordId { get; set; }
            public int CategoryId { get; set; }
            public string TrackingNumber { get; set; } = null!;
            public int? ComplainantId { get; set; }
            public int? RespondentId { get; set; }
            public DateTime IncidentDate { get; set; }
            public DateTime? ReportedDate { get; set; }
            public string Location { get; set; } = null!;
            public string Narrative { get; set; } = null!;
            public string Status { get; set; } = null!;
        }

        public class CreateBlotterRecordDTO : IValidatableObject
        {
            [Range(1, int.MaxValue)]
            public int CategoryId { get; set; }

            public int? ComplainantId { get; set; }

            public int? RespondentId { get; set; }

            public DateTime IncidentDate { get; set; }

            [Required]
            [StringLength(255)]
            public string Location { get; set; } = null!;

            [Required]
            [StringLength(12000)]
            public string Narrative { get; set; } = null!;

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (IncidentDate == default)
                    yield return new ValidationResult("An incident date and time is required.", new[] { nameof(IncidentDate) });
            }
        }

        public class UpdateBlotterRecordDTO
        {
            [Required]
            public string Narrative { get; set; } = null!;

            [Required]
            [RegularExpression(
                "^(Pending|Ongoing|Resolved|Dismissed)$",
                ErrorMessage = "Status must be Pending, Ongoing, Resolved or Dismissed.")]
            public string Status { get; set; } = null!;

            public DateTime? ResolutionDate { get; set; }
        }
    }
}