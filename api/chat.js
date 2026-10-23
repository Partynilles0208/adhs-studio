// Vercel Serverless Function: Groq / OpenAI Chat Completion
// Model: openai/gpt-oss-20b
export default async function handler(req, res) {
  // Enable CORS
  res.setHeader('Access-Control-Allow-Credentials', true);
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,OPTIONS,PATCH,DELETE,POST,PUT');
  res.setHeader(
    'Access-Control-Allow-Headers',
    'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version, Authorization'
  );

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { messages, model = 'openai/gpt-oss-20b' } = req.body || {};

  // Groq API Key or OpenAI API Key from Vercel Environment Variables
  const apiKey = process.env.GROQ_API_KEY || process.env.OPENAI_API_KEY;

  if (!apiKey) {
    return res.status(500).json({
      error: 'Kein API Key auf Vercel hinterlegt. Bitte GROQ_API_KEY in den Vercel Environment Variables anlegen.'
    });
  }

  try {
    const isGroq = Boolean(process.env.GROQ_API_KEY);
    const endpoint = isGroq
      ? 'https://api.groq.com/openai/v1/chat/completions'
      : 'https://api.openai.com/v1/chat/completions';

    const response = await fetch(endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify({
        model: model || 'openai/gpt-oss-20b',
        messages: messages || [],
        temperature: 0.7,
        max_tokens: 1500
      })
    });

    const data = await response.json();

    if (!response.ok) {
      // If the specific model isn't available on Groq account, attempt fallback
      if (isGroq && data.error && data.error.code === 'model_not_found') {
        const fallbackRes = await fetch(endpoint, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${apiKey}`
          },
          body: JSON.stringify({
            model: 'llama-3.3-70b-versatile',
            messages: messages || [],
            temperature: 0.7,
            max_tokens: 1500
          })
        });
        const fallbackData = await fallbackRes.json();
        return res.status(fallbackRes.status).json(fallbackData);
      }
      return res.status(response.status).json(data);
    }

    return res.status(200).json(data);
  } catch (err) {
    return res.status(500).json({ error: err.message || 'Serverfehler beim Aufruf der Groq/OpenAI API' });
  }
}
