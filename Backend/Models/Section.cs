using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Section
    {
        public Section()
        {
            Topics = new List<Topic>();
        }

        public int Id { get; set; }

        [Required]
        public required string SectionName { get; set; }

        public ICollection<Topic> Topics { get; set; }
    }
} 