namespace UrjaAPI.Model
{
    public class StatePointOfInterestDto
    {
        public int Id { get; set; }
        #region WhyThisQuestinMark
        //warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
        #endregion
        public string? Name { get; set; }=string.Empty;
        public string? Description { get; set; } = null;
    }
}
