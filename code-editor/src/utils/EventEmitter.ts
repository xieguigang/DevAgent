namespace CodeEditor.Utils {
    /**
     * Minimal event emitter used internally for editor events.
     */
    export class EventEmitter<T> {
        private listeners: Array<(data: T) => void> = [];

        on(listener: (data: T) => void): void {
            this.listeners.push(listener);
        }

        off(listener: (data: T) => void): void {
            const idx = this.listeners.indexOf(listener);
            if (idx >= 0) {
                this.listeners.splice(idx, 1);
            }
        }

        emit(data: T): void {
            for (const l of this.listeners.slice()) {
                l(data);
            }
        }
    }
}
