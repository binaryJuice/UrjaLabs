// <copyright file="StateDataSource.cs" company="binaryjuice">
// Copyright (c) binaryjuice. All rights reserved.
// </copyright>

namespace UrjaAPI
{
    using UrjaAPI.Model;

    /// <summary>
    /// Inline data store
    /// </summary>
    public class StateDataSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateDataSource"/> class.
        /// Inline data source for states and its point of interest.
        /// </summary>
        public StateDataSource()
        {
            this.State = new List<StateDto>()
            {
                new StateDto() { Id = 1, Name = "PA", PointOfInterestDtos =new List<StatePointOfInterestDto>() { new StatePointOfInterestDto() { Id = 17 } } },
                new StateDto() { Id = 2, Name = "PA", PointOfInterestDtos=new List<StatePointOfInterestDto>() { new StatePointOfInterestDto() { Id = 72 } } },
                new StateDto() { Id = 3, Name = "PA", PointOfInterestDtos=new List<StatePointOfInterestDto>() { new StatePointOfInterestDto() { Id = 23 } } },
                new StateDto() { Id = 4, Name = "PA", PointOfInterestDtos = new List<StatePointOfInterestDto>() { new StatePointOfInterestDto() { Id = 26 } } },
            };
        }

        public static StateDataSource Current { get; } = new StateDataSource();

        public List<StateDto> State { get; set; }
    }
}
