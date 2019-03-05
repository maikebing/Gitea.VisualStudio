﻿using Gitea.VisualStudio.Shared;
using Gitea.VisualStudio.Shared.Controls;
using System;

namespace Gitea.VisualStudio.UI.ViewModels
{
    public class Owner : IEquatable<Owner>
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string DisplayName { get => string.IsNullOrWhiteSpace(FullName) ? Name : FullName; }
        public string AvatarUrl { get; set; }
        public bool IsExpanded { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var p = (Owner)obj;
            return Name == p.Name;
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public bool Equals(Owner other)
        {
            return Name == other.Name;
        }
    }

    public class ProjectViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public Owner Owner { get; set; }
        public Octicon Icon { get; set; }
        public System.Windows.Visibility DescriptionVisibility
        {
            get
            {
                return string.IsNullOrWhiteSpace(Description) 
                    ? System.Windows.Visibility.Collapsed 
                    : System.Windows.Visibility.Visible;
            }
        }
        public bool IsActive { get; set; }

        public ProjectViewModel(Project repository)
        {
            Name = repository.Name;
            Url = repository.Url;
            Description = repository.Description;

            if (repository.Owner != null)
            {
                Owner = new Owner
                {
                    Name = repository.Owner.Name,
                    FullName = repository.Owner.FullName,
                    AvatarUrl = repository.Owner.AvatarUrl
                };
            }

            Icon = repository.Icon;
        }
    }
}
