﻿using Bakalauras.Auth;
using System.ComponentModel.DataAnnotations;

namespace Bakalauras.data.entities
{
    public class Chapter: IUserOwnedResource
    {
        private int _Id;
        private string _Name;
        private string _Content;
        private int _BookId;

        public int Id { get; set; }
        [Required]
        public int BookId { get; set; }

        [Required]
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }
    }
}
