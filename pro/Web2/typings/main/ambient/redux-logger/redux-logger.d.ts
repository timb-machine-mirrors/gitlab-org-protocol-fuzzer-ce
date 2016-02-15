// Compiled using typings@0.6.8
// Source: https://raw.githubusercontent.com/DefinitelyTyped/DefinitelyTyped/7b3d5a6ea5acda4a81f71309f1cc62354e63b681/redux-logger/redux-logger.d.ts
// Type definitions for redux-logger v2.0.0
// Project: https://github.com/fcomb/redux-logger
// Definitions by: Alexander Rusakov <https://github.com/arusakov/>
// Definitions: https://github.com/borisyankov/DefinitelyTyped


declare module 'redux-logger' {

  interface ReduxLoggerOptions {
    actionTransformer?: (action: any) => any;
    collapsed?: boolean;
    duration?: boolean;
    level?: string;
    logger?: any;
    predicate?: (getState: Function, action: any) => boolean;
    timestamp?: boolean;
    transformer?: (state:any) => any;
  }

  export default function createLogger(options?: ReduxLoggerOptions): Redux.Middleware;
}