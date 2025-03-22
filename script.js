document.addEventListener("DOMContentLoaded", function () {
    fetchDivisions();
    
    document.getElementById("downloadForm").addEventListener("submit", function (event) {
        event.preventDefault();
        startDownload();
    });
});

async function fetchDivisions() {
    try {
        const response = await fetch("/api/divisions");
        const divisions = await response.json();
        const divisionSelect = document.getElementById("division");

        divisions.forEach(div => {
            let option = document.createElement("option");
            option.value = div.id;
            option.textContent = div.name;
            divisionSelect.appendChild(option);
        });
    } catch (error) {
        console.error("Error fetching divisions:", error);
    }
}

async function startDownload() {
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;
    const division = document.getElementById("division").value;
    const user = document.getElementById("user").value;
    const queue = document.getElementById("queue").value;
    
    const statusText = document.getElementById("status");
    const progressBar = document.getElementById("progressBar");

    statusText.textContent = "Starting download...";
    progressBar.style.width = "0%";
    progressBar.textContent = "0%";

    try {
        const response = await fetch("/api/download", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ startDate, endDate, division, user, queue })
        });

        if (!response.ok) throw new Error("Failed to start download");

        const reader = response.body.getReader();
        let receivedLength = 0;
        const contentLength = +response.headers.get("Content-Length");

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            receivedLength += value.length;
            const percent = Math.floor((receivedLength / contentLength) * 100);
            progressBar.style.width = percent + "%";
            progressBar.textContent = percent + "%";
        }

        statusText.textContent = "Download complete!";
    } catch (error) {
        console.error("Error:", error);
        statusText.textContent = "Download failed!";
    }
}
