const webview2 = chrome.webview;
const $ = function (id) {
  return document.getElementById(id);
};

if (!webview2) {
  throw "web application is not running on webview2 host environment!";
}

const devkit = webview2.hostObjects.devkit;
