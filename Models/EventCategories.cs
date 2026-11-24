namespace CollegeEventManagement.Models
{
    // Fixed lists used by forms, filters and charts.
    public static class EventCategories
    {
        public static readonly string[] All =
            { "Technical", "Robotics", "Hackathons", "Cultural", "Sports", "Workshops", "Seminars", "Entrepreneurship", "Arts" };

        // CSS-friendly name, e.g. "Technical" -> "technical"
        public static string Slug(string category) =>
            string.IsNullOrWhiteSpace(category) ? "technical" : category.ToLowerInvariant();

        // Lucide icon id for each category
        public static string Icon(string category) => Slug(category) switch
        {
            "technical" => "cpu",
            "robotics" => "bot",
            "hackathons" => "code-xml",
            "cultural" => "music",
            "sports" => "trophy",
            "workshops" => "wrench",
            "seminars" => "presentation",
            "entrepreneurship" => "rocket",
            "arts" => "palette",
            _ => "sparkles"
        };

        public static readonly string[] Departments =
            { "Computer Science", "Electronics", "Mechanical", "Civil", "Management", "Design and Arts", "Sciences", "Other" };
    }
}
