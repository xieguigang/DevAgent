"use strict";
var CodeEditor;
(function (CodeEditor) {
    var Utils;
    (function (Utils) {
        /**
         * Minimal event emitter used internally for editor events.
         */
        class EventEmitter {
            constructor() {
                this.listeners = [];
            }
            on(listener) {
                this.listeners.push(listener);
            }
            off(listener) {
                const idx = this.listeners.indexOf(listener);
                if (idx >= 0) {
                    this.listeners.splice(idx, 1);
                }
            }
            emit(data) {
                for (const l of this.listeners.slice()) {
                    l(data);
                }
            }
        }
        Utils.EventEmitter = EventEmitter;
    })(Utils = CodeEditor.Utils || (CodeEditor.Utils = {}));
})(CodeEditor || (CodeEditor = {}));
