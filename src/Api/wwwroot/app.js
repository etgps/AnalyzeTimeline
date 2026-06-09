const inputScreen = document.querySelector("#inputScreen");
const outputScreen = document.querySelector("#outputScreen");
const uploadForm = document.querySelector("#uploadForm");
const fileInput = document.querySelector("#timelineFile");
const fileName = document.querySelector("#fileName");
const message = document.querySelector("#message");
const startButton = document.querySelector("#startButton");
const previewFrame = document.querySelector("#previewFrame");
const downloadButton = document.querySelector("#downloadButton");
const backButton = document.querySelector("#backButton");
const summaryText = document.querySelector("#summaryText");

let currentReportHtml = "";
const maxUploadBytes = 512 * 1024 * 1024;

fileInput.addEventListener("change", () => {
  fileName.textContent = fileInput.files.length > 0
    ? fileInput.files[0].name
    : "ファイルが選択されていません";
  message.textContent = "";
});

uploadForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  if (fileInput.files.length === 0) {
    message.textContent = "Timeline.json を選択してください。";
    return;
  }

  if (fileInput.files[0].size > maxUploadBytes) {
    message.textContent = "512MB 以下の Timeline.json を選択してください。";
    return;
  }

  startButton.disabled = true;
  startButton.textContent = "分析中...";
  message.textContent = "";

  try {
    const formData = new FormData();
    formData.append("file", fileInput.files[0]);

    const response = await fetch("/api/timeline/analyze", {
      method: "POST",
      body: formData
    });

    const payload = await readJsonOrFallback(response);
    if (!response.ok) {
      throw new Error(payload.message ?? "分析に失敗しました。");
    }

    currentReportHtml = payload.html;
    previewFrame.srcdoc = currentReportHtml;
    summaryText.textContent = `抽出地点 ${payload.parsedLocationCount} / 分類済み ${payload.classifiedLocationCount}`;
    showOutput();
  } catch (error) {
    message.textContent = error.message === "Failed to fetch"
      ? "サーバーに接続できませんでした。アプリが起動中か、アップロード上限を超えていないか確認してください。"
      : error.message;
  } finally {
    startButton.disabled = false;
    startButton.textContent = "分析開始";
  }
});

downloadButton.addEventListener("click", () => {
  const blob = new Blob([currentReportHtml], { type: "text/html;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = "timeline-visit-report.html";
  anchor.click();
  URL.revokeObjectURL(url);
});

backButton.addEventListener("click", () => {
  outputScreen.classList.remove("active");
  inputScreen.classList.add("active");
});

function showOutput() {
  inputScreen.classList.remove("active");
  outputScreen.classList.add("active");
}

async function readJsonOrFallback(response) {
  const text = await response.text();
  if (text.length === 0) {
    return { message: response.ok ? "" : `分析に失敗しました。HTTP ${response.status}` };
  }

  try {
    return JSON.parse(text);
  } catch {
    return { message: response.ok ? "" : `分析に失敗しました。HTTP ${response.status}` };
  }
}
