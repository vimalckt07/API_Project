using Microsoft.Extensions.Caching.Memory;
using NewsAPI.Model;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace NewsAPI.Services
{
    /// <summary>
    /// Service to fetch the newest stories from Hacker News API.
    /// </summary>
    public interface IHackerNewsService
    {
        Task<IEnumerable<Story>> GetNewestStoriesAsync();
    }
    /// <summary>
    /// Initializes a new instance of the HackerNewsService.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public class HackerNewsService : IHackerNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HackerNewsService> _logger;
        private const string CacheKey = "NewStories";
        private const string HackerNewsUrl = "https://hacker-news.firebaseio.com/v0/newstories.json";
        private const string StoryUrl = "https://hacker-news.firebaseio.com/v0/item/{0}.json";

        public HackerNewsService(IMemoryCache cache, HttpClient httpClient, ILogger<HackerNewsService> logger)
        {
            _cache = cache;
            _httpClient = httpClient;
            _logger = logger;
        }
        /// <summary>
        /// Fetches the newest stories from the Hacker News.
        /// </summary>
        /// <returns>A list of the newest stories are.</returns>
        public async Task<IEnumerable<Story>> GetNewestStoriesAsync()
        {
            _logger.LogInformation("Fetching newest stories from cache or API.");


            if (!_cache.TryGetValue(CacheKey, out List<Story>? stories))
            {

                try
                {
                    _logger.LogInformation("Cache miss. Fetching stories from Hacker News API.");

                var storyIds = await _httpClient.GetStringAsync(HackerNewsUrl);
                List<int>? ids;

                try
                {
                    ids = JsonConvert.DeserializeObject<List<int>>(storyIds);
                }
                catch (JsonException)
                {
                    return new List<Story>();
                }
                stories = new List<Story>();
                var top200Ids = ids.Take(200);
                foreach (var id in top200Ids)
                {
                    var story = await GetStoryAsync(id);
                    if (story != null)
                    {
                        stories.Add(story);
                    }
                }

                _cache.Set(CacheKey, stories, TimeSpan.FromMinutes(10));

                _logger.LogInformation("Stories fetched and cached successfully.");
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while fetching stories from the API.");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Cache hit. Returning cached stories.");
            }

            return stories;
        }

        /// <summary>
        /// Fetches a story by its ID from Hacker News.
        /// </summary>
        /// <param name="id">The story ID.</param>
        /// <returns>The story, or null if an error occurred.</returns>
        private async Task<Story> GetStoryAsync(int id)
        {
            var storyJson = await _httpClient.GetStringAsync(string.Format(StoryUrl, id));

            try
            {
                _logger.LogInformation($"Fetching story with ID {id}.");
                return JsonConvert.DeserializeObject<Story>(storyJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch story with ID {id}.");
                return null;
            }
        }
    }
}
