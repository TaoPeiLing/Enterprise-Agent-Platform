using System;
using System.IO; // For MemoryStream
using System.Text; // For StringBuilder
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Enterprise.Agent.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        public async Task<string> ExtractTextFromPdfAsync(byte[] pdfDocumentStream)
        {
            if (pdfDocumentStream == null || pdfDocumentStream.Length == 0)
            {
                // Return string.Empty directly as the method now returns Task<string>
                // and async machinery handles wrapping it.
                return string.Empty; 
            }

            try
            {
                // PdfPig works with streams.
                using (var memoryStream = new MemoryStream(pdfDocumentStream))
                {
                    // Asynchronously process the PDF using Task.Run for CPU-bound work.
                    // This is good practice to avoid blocking the calling thread, especially in server environments.
                    return await Task.Run(() => 
                    {
                        using (PdfDocument document = PdfDocument.Open(memoryStream))
                        {
                            StringBuilder fullText = new StringBuilder();
                            foreach (Page page in document.GetPages())
                            {
                                fullText.Append(page.Text);
                                // Optionally, add a space or newline between pages
                                if (page.Number < document.NumberOfPages)
                                {
                                    fullText.AppendLine(); 
                                }
                            }
                            return fullText.ToString();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using ILogger if available, or rethrow as a custom exception)
                Console.WriteLine($"Error extracting text from PDF: {ex.Message}"); // Replace with proper logging
                // Depending on error handling strategy, either return empty, null, or throw
                // For now, returning empty string as per the example's implication for robustness.
                return string.Empty; 
                // Or throw a new custom exception, e.g.: 
                // throw new PdfProcessingException("Failed to extract text from PDF.", ex);
            }
        }
    }

    // Example of a custom exception (if you choose to throw one)
    // public class PdfProcessingException : Exception
    // {
    //     public PdfProcessingException(string message, Exception innerException) : base(message, innerException) { }
    // }
}
