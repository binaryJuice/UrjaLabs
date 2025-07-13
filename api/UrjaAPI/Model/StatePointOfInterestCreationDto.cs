// <copyright file="StatePointOfInterestCreationDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace UrjaAPI.Model
{
    /// <summary>
    /// This the viewModel involved in the creation.
    /// </summary>
    public class StatePointOfInterestCreationDto
    {
        // Name of point of interest.
        public string? Name { get; set; } = string.Empty;

        // Description of the point of interest.
        public string? Description { get; set; } = null;
    }
}
