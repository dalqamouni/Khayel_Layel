const express = require("express");
const { exec } = require("child_process");
const path = require("path");
const fs = require("fs");

const app = express();
const PORT = 3000;

app.use(express.static("public")); // Serve the HTML file

app.get("/download", (req, res) => {
    const outputFilePath = path.join(__dirname, "recordings.zip"); // Path to save recordings

    // Run the Genesys CLI command to download recordings
    exec("genesys-cloud-recordings-downloader --output recordings.zip", (error, stdout, stderr) => {
        if (error) {
            console.error(`Error downloading recordings: ${error.message}`);
            return res.status(500).send("Download failed.");
        }

        // Check if the file exists before sending
        if (fs.existsSync(outputFilePath)) {
            res.download(outputFilePath, "recordings.zip", (err) => {
                if (err) console.error("Error sending file:", err);
                fs.unlinkSync(outputFilePath); // Delete file after download
            });
        } else {
            res.status(500).send("File not found.");
        }
    });
});

app.listen(PORT, () => console.log(`Server running at http://localhost:${PORT}`));
