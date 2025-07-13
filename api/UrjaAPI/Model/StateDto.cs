namespace UrjaAPI.Model
{
    /// <summary>
    /// State dto representing all the states of USA.
    /// </summary>
    public class StateDto
    {
        // unique id for state.
        public int Id { get; set; }

        #region WhyThisQuestinMark
        //warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
        #endregion

        // Name of the state.
        public string? Name { get; set; }

        public ICollection<StatePointOfInterestDto> PointOfInterestDtos { get; set; } = new List<StatePointOfInterestDto>();

        public int CountPointOfInterest
        {
            get { return this.PointOfInterestDtos.Count; }
        }
    }
}
