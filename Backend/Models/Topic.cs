using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Topic
    {
        public int Id { get; set; }

        [Required]
        public required string TopicName { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public int Difficulty { get; set; }

        [Required]
        public int TimeLimit { get; set; }

        [Required]
        public required string AuthorName { get; set; }

        [Required]
        [EmailAddress]
        public required string AuthorEmail { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public int SectionId { get; set; }
        public Section? Section { get; set; }
    }
} 