// Vercel Serverless Function: OpenAI Image Generation
// Model: gpt-image-1-mini / DALL-E
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

  const { prompt, model = 'gpt-image-1-mini', size = '1024x1024' } = req.body || {};

  const apiKey = process.env.OPENAI_API_KEY;

  if (!apiKey) {
    return res.status(500).json({
      error: 'Kein OpenAI API Key auf Vercel hinterlegt. Bitte OPENAI_API_KEY in den Vercel Environment Variables anlegen.'
    });
  }

  if (!prompt || typeof prompt !== 'string') {
    return res.status(400).json({ error: 'Kein Prompt übergeben.' });
  }

  try {
    const response = await fetch('https://api.openai.com/v1/images/generations', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify({
        model: model || 'gpt-image-1-mini',
        prompt: prompt,
        n: 1,
        size: size || '1024x1024'
      })
    });

    const data = await response.json();

    if (!response.ok) {
      // If gpt-image-1-mini isn't directly recognized in standard OpenAI account, fallback to dall-e-3
      if (data.error && (data.error.code === 'model_not_found' || data.error.message?.includes('model'))) {
        const fallbackRes = await fetch('https://api.openai.com/v1/images/generations', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${apiKey}`
          },
          body: JSON.stringify({
            model: 'dall-e-3',
            prompt: prompt,
            n: 1,
            size: '1024x1024'
          })
        });
        const fallbackData = await fallbackRes.json();
        return res.status(fallbackRes.status).json(fallbackData);
      }
      return res.status(response.status).json(data);
    }

    return res.status(200).json(data);
  } catch (err) {
    return res.status(500).json({ error: err.message || 'Serverfehler beim Aufruf der OpenAI Image API' });
  }
}
