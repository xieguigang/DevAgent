namespace CodeEditor {

    export interface IDevKit {
        updateStatus(line: string, col: string, lang: string, file: string): void;

    }

    export const devkit: IDevKit = (function (): any {
        try {
            return chrome.webview.hostObjects.devkit;
        } catch (ex) {
            return null;
        }
    })();
}