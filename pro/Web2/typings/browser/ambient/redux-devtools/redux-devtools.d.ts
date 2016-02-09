// Compiled using typings@0.6.6
// Source: https://raw.githubusercontent.com/DefinitelyTyped/DefinitelyTyped/7b3d5a6ea5acda4a81f71309f1cc62354e63b681/redux-devtools/redux-devtools.d.ts
// Type definitions for redux-devtools 3.0.0
// Project: https://github.com/gaearon/redux-devtools
// Definitions by: Petryshyn Sergii <https://github.com/mc-petry>
// Definitions: https://github.com/DefinitelyTyped/DefinitelyTyped


declare module "redux-devtools" {
  import * as React from 'react'

  interface IDevTools {
    new (): JSX.ElementClass
    instrument(): Function
  }

  export function createDevTools(el: React.ReactElement<any>): IDevTools
  export function persistState(debugSessionKey: string): Function

  var factory: { instrument(): Function }

  export default factory;
}