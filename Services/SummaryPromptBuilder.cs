namespace Pretest_GenAI.Services
{
    // Xây dựng prompt cho Gemini theo yêu cầu R4: <= 100 từ, 3-5 key points, định dạng JSON, cấm bịa đặt thông tin
    public static class SummaryPromptBuilder
    {
        public static string Build(string content)
        {
            return $@"You are an AI document summarizer. Summarize the following document content strictly according to these requirements:
1. Provide a concise summary of the content in no more than 100 words.
2. Generate 3 to 5 key points highlighting the essential facts.
3. Prohibit invented facts: do not invent, assume, or extrapolate any details that are not directly stated in the text.
4. Output MUST be a valid JSON object matching the following format:
{{
  ""summary"": ""<summary text, maximum 100 words>"",
  ""keyPoints"": [
    ""<key point 1>"",
    ""<key point 2>"",
    ""<key point 3>""
  ]
}}

Return only the JSON object without any additional markdown formatting or explanation outside the JSON.

Content to summarize:
{content}";
        }
    }
}
