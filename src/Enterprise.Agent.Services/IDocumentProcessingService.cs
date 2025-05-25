using System.Threading.Tasks;

namespace Enterprise.Agent.Services
{
    public interface IDocumentProcessingService
    {
        Task<string> ExtractTextFromPdfAsync(byte[] pdfDocumentStream);
        // Future methods: ConvertToText, ExtractMetadata, etc.
    }
}
