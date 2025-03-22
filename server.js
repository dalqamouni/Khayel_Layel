const express = require("express");
const axios = require("axios");
const cors = require("cors");
require("dotenv").config();

const app = express();
app.use(express.json());
app.use(cors());

const GENESYS_CLIENT_ID = process.env.GENESYS_CLIENT_ID;
const GENESYS_CLIENT_SECRET = process.env.GENESYS_CLIENT_SECRET;
const GENESYS_REGION = process.env.GENESYS_REGION;

let authToken = "";

// Authenticate with Genesys Cloud
async function authenticate() {
    const response = await axios.post(`https://login.${GENESYS_REGION}/oauth/token`, null, {
        params: {
            grant_type: "client_credentials",
            client_id: GENESYS_CLIENT_ID,
            client_secret: GENESYS_CLIENT_SECRET
        }
    });
    authToken = response.data.access_token;
}

// Fetch available divisions
app.get("/api/divisions", async (req, res) => {
    try {
        await authenticate();
        const response = await axios.get(`https://api.${GENESYS_REGION}/api/v2/authorization/divisions`, {
            headers: { Authorization: `Bearer ${authToken}` }
        });
        res.json(response.data.entities);
    } catch (error) {
        console.error("Error fetching divisions:", error);
        res.status(500).json({ error: "Failed to fetch divisions" });
    }
});

// Start downloading recordings
app.post("/api/download", async (req, res) => {
    try {
        const { startDate, endDate, division, user, queue } = req.body;
        await authenticate();

        // Build query params
        let query = `startDate=${startDate}&endDate=${endDate}&divisionId=${division}`;
        if (user !== "all") query += `&userId=${user}`;
        if (queue !== "all") query += `&queueId=${queue}`;

        // Request recordings from Genesys
        const response = await axios.get(`https://api.${GENESYS_REGION}/api/v2/conversations/recordings?${query}`, {
            headers: { Authorization: `Bearer ${authToken}` }
        });

        const recordings = response.data.entities;

        if (recordings.length === 0) {
            return res.status(404).json({ error: "No recordings found" });
        }

        // Send recording data (simulate download process)
        res.setHeader("Content-Type", "application/octet-stream");
        res.setHeader("Content-Disposition", 'attachment; filename="recordings.zip"');

        for (let i = 0; i < recordings.length; i++) {
            res.write(`Downloading ${recordings[i].id}...\n`);
            await new Promise(resolve => setTimeout(resolve, 500)); // Simulate download delay
        }
        res.end();
    } catch (error) {
        console.error("Download error:", error);
        res.status(500).json({ error: "Download failed" });
    }
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});
