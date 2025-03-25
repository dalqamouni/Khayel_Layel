const axios = require('axios');
const fs = require('fs');
const path = require('path');
const { setTimeout } = require('timers/promises');

const clientId = "485e10e6-44be-4a4f-adf5-f4661cdaa335";
const clientSecret = "dqakz-pDeRaekvSEVfFtz8dgbeLqHNdasIhEFOJpmG4";
const orgRegion = "eu-west-1";
const dates = "2025-02-25T00:00:00.000Z/2025-02-25T23:59:00.000Z";
const folderName = "2025-02-25";
const baseApiUrl = "https://api.mypurecloud.com";

let accessToken = "";

async function authenticate() {
    const tokenUrl = `${baseApiUrl}/oauth/token`;
    const response = await axios.post(tokenUrl, null, {
        auth: { username: clientId, password: clientSecret },
        params: { grant_type: 'client_credentials' }
    });
    accessToken = response.data.access_token;
    console.log("Authenticated successfully");
}

async function getConversations(filter) {
    const response = await axios.post(
        `${baseApiUrl}/api/v2/analytics/conversations/details/query`,
        {
            filter: filter,
            interval: dates,
            paging: { pageSize: 100, pageNumber: 1 }
        },
        { headers: { Authorization: `Bearer ${accessToken}` } }
    );
    return response.data.conversations;
}

async function getRecordings(conversationId) {
    const response = await axios.get(
        `${baseApiUrl}/api/v2/conversations/${conversationId}/recordings`,
        { headers: { Authorization: `Bearer ${accessToken}` } }
    );
    return response.data;
}

async function downloadRecording(url, filename) {
    const response = await axios({ url, method: 'GET', responseType: 'stream' });
    const dir = path.join(__dirname, folderName);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    
    const filePath = path.join(dir, filename);
    response.data.pipe(fs.createWriteStream(filePath));
    console.log(`Downloaded: ${filename}`);
}

async function processRecordings() {
    await authenticate();
    const conversations = await getConversations({ dimension: "divisionId", value: "89c0e97b-f8c1-477a-ae0c-97f4ca394702" });
    
    for (const convo of conversations) {
        const recordings = await getRecordings(convo.conversationId);
        for (const recording of recordings) {
            await downloadRecording(recording.resultUrl, `${convo.conversationId}_${recording.id}.mp3`);
            await setTimeout(2000);
        }
    }
    console.log("All downloads complete.");
}

processRecordings().catch(console.error);
