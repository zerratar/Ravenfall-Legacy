namespace RavenNest.SDK.Endpoints
{
    public class UpdatePlayerExperience
    {
        public UpdatePlayerExperience(string userId, decimal[] experience)
        {
            UserId = userId;
            Experience = experience;
        }

        public string UserId { get; }
        public decimal[] Experience { get; }
    }
}