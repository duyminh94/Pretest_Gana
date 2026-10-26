namespace Pretest_GenAI.Services
{
    // Ngoại lệ khi AI trả về kết quả không hợp lệ theo quy tắc của R4
    public class AiValidationException : Exception
    {
        public AiValidationException(string message) : base(message)
        {
        }
    }

    // Ngoại lệ khi cấu hình Gemini API key bị thiếu
    public class AiConfigurationException : Exception
    {
        public AiConfigurationException(string message) : base(message)
        {
        }
    }
}
