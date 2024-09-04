using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace AILabb2;

internal class AiService
{
    private ApiKeyServiceClientCredentials credentials;
    private string cogSvcKey;
    private string cogSvcEndpoint;
    private string region;
    private ComputerVisionClient cvClient;

    //path to image
    private string imageFile;

    public async Task Init()
    {
        Console.Write("Enter key:> ");
        cogSvcKey = Console.ReadLine();
        Console.Write("Enter endpoint:> ");
        cogSvcEndpoint = Console.ReadLine();

        credentials = new(cogSvcKey);
        cvClient = new ComputerVisionClient(credentials)
        {
            Endpoint = cogSvcEndpoint
        };

        //user input image path
        await Question();
    }

    public async Task Question()
    {
        Console.Write("Enter URL:> ");
        imageFile = Console.ReadLine();

        List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
        {
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Tags,
            VisualFeatureTypes.Objects
        };

        Console.Clear();
        Console.WriteLine("Analyzing, please hold.");
        //is path URL or local?
        switch (await IsImageUrl())
        {
            case true:
                //method URL for analyze and print info
                await ImageAnalyzeUrl(features);
                //method URL for generating thumbnail
                await GenerateThumbnailUrl();
                break;

            case false:
                //method local for analyze and print info
                await ImageAnalyzeLocal(features);
                //method local for generating thumbnail
                await GenerateThumbnailLocal();
                break;
        }
    }

    async Task<bool> IsImageUrl()
    {
        if (imageFile[0] == '\"' && imageFile[^1] == '\"')
        {
            imageFile = imageFile[1..^1];
        }

        using var httpClient = new HttpClient();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, imageFile);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            return contentType?.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }
        catch
        {
            return false; // Consider handling specific exceptions for better error handling
        }

    }

    public async Task ImageAnalyzeUrl(List<VisualFeatureTypes?> features)
    {

        var analysis = await cvClient.AnalyzeImageAsync(imageFile, features);

        //Descriptions
        foreach (var caption in analysis.Description.Captions)
        {
            Console.WriteLine($"Description: {caption.Text} (Confidence: {caption.Confidence.ToString("P")})");
        }

        //Tags
        if (analysis.Tags.Count > 0)
        {
            Console.WriteLine("\nTags:");
            foreach (var tag in analysis.Tags)
            {
                Console.WriteLine($" -{tag.Name} (Confidence: {tag.Confidence.ToString("P")})");
            }
        }

        //Objects
        if (analysis.Objects.Count > 0)
        {
            Console.WriteLine("\nObjects:");

            foreach (var detectedObject in analysis.Objects)
            {
                Console.WriteLine($" -{detectedObject.ObjectProperty} (Confidence: {detectedObject.Confidence.ToString("P")})");
            }
        }
    }

    public async Task GenerateThumbnailUrl()
    {
        try
        {
            var thumbnailStream = await cvClient.GenerateThumbnailAsync(100, 100, imageFile, true);

            string thumbnailFileName = "thumbnail.png";
            using (Stream thumbnailFile = File.Create(thumbnailFileName))
            {
                await thumbnailStream.CopyToAsync(thumbnailFile);
            }

            Console.WriteLine($"\nThumbnail saved as: {thumbnailFileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task ImageAnalyzeLocal(List<VisualFeatureTypes?> features)
    {
        using (var imageData = File.OpenRead(imageFile))
        {
            var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

            //Descriptions
            foreach (var caption in analysis.Description.Captions)
            {
                Console.WriteLine($"Description: {caption.Text} (Confidence: {caption.Confidence.ToString("P")})");
            }

            //Tags
            if (analysis.Tags.Count > 0)
            {
                Console.WriteLine("\nTags:");
                foreach (var tag in analysis.Tags)
                {
                    Console.WriteLine($" -{tag.Name} (Confidence: {tag.Confidence.ToString("P")})");
                }
            }

            //Objects
            if (analysis.Objects.Count > 0)
            {
                Console.WriteLine("\nObjects:");

                foreach (var detectedObject in analysis.Objects)
                {
                    Console.WriteLine($" -{detectedObject.ObjectProperty} (Confidence: {detectedObject.Confidence.ToString("P")})");
                }
            }
        }
    }

    public async Task GenerateThumbnailLocal()
    {
        try
        {
            using (var imageData = File.OpenRead(imageFile))
            {
                var thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(100, 100, imageData, true);

                string thumbnailFileName = "thumbnail.png";
                using (Stream thumbnailFile = File.Create(thumbnailFileName))
                {
                    await thumbnailStream.CopyToAsync(thumbnailFile);
                }

                Console.WriteLine($"\nThumbnail saved as: {thumbnailFileName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}