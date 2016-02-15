// Compiled using typings@0.6.8
// Source: https://raw.githubusercontent.com/DefinitelyTyped/DefinitelyTyped/14700203a4c18ec5f7739dc7478e7508069ad9fb/redux-saga/redux-saga.d.ts
// Type definitions for redux-saga 0.6.0
// Project: https://github.com/yelouafi/redux-saga
// Definitions by: Daniel Lytkin <https://github.com/aikoven>
// Definitions: https://github.com/borisyankov/DefinitelyTyped


declare module 'redux-saga' {
  import {Middleware} from 'redux';
  import {Task} from 'redux-saga/effects';

  export type Saga = <T>(getState?: () => T) => Iterable<any>;
  export default function(...sagas: Saga[]): Middleware;
  
  export class SagaCancellationException {}
  export function isCancelError(error): boolean;

  export { runSaga, storeIO } from 'redux-saga/internal/runSaga';

  export { takeEvery, takeLatest } from 'redux-saga/internal/sagaHelpers';

  import * as effects from 'redux-saga/effects';
  import * as utils from 'redux-saga/utils';

  export {
    effects,
    utils
  }
}

declare module 'redux-saga/effects' {
  export type Effect = {};

  type Predicate = (action: any) => boolean;
  
  export interface Task<T> {
    name: string;
    isRunning(): boolean;
    result(): T;
    error(): any;
  }

  export function take(pattern?: string | string[] | Predicate): Effect;

  export function put(action: any): Effect;

  export function race(effects: { [key: string]: any }): Effect;

  export function call<T1, T2, T3>(fn: (arg1?: T1, arg2?: T2, arg3?: T3, ...rest: any[]) => any,
    arg1?: T1, arg2?: T2, arg3?: T3, ...rest: any[]): Effect;

  // apply
  // cps

  export function fork(effect: Effect): Effect;
  export function fork<T1, T2, T3>(fn: (arg1?: T1, arg2?: T2, arg3?: T3, ...rest: any[]) =>
    Promise<any> | Iterable<any>,
    arg1?: T1, arg2?: T2, arg3?: T3, ...rest: any[]): Effect;

  export function join(task: Task<any>): Effect;

  export function cancel(task: Task<any>): Effect;
}


declare module 'redux-saga/utils' {
  import {Task} from 'redux-saga/effects';

  const CANCEL: symbol;
  const RACE_AUTO_CANCEL: string;
  const PARALLEL_AUTO_CANCEL: string;
  const MANUAL_CANCEL: string;

  import * as monitorActions from 'redux-saga/internal/monitorActions';

  export {
    // TASK
    // noop
    // is, asEffect
    // deferred
    // arrayOfDeffered
    // asap

    CANCEL,
    RACE_AUTO_CANCEL,
    PARALLEL_AUTO_CANCEL,
    MANUAL_CANCEL,

    // createMockTask

    monitorActions
  }
}

declare module 'redux-saga/internal/runSaga' {
  import {Store} from 'redux';
  import {Task} from 'redux-saga/effects';
  
  interface IO {
    dispatch: (action: any) => any;
    subscribe: (cb: Function) => Function;
  }

  export function storeIO(store: Store): IO;
  export function runSaga(iterator: Iterable<any>,
    io: IO,
    monitor?: (action: any) => void): Task<any>;
}

declare module 'redux-saga/internal/monitorActions' {
  export const MONITOR_ACTION: string;
  export const EFFECT_TRIGGERED: string;
  export const EFFECT_RESOLVED: string;
  export const EFFECT_REJECTED: string;
}

declare module 'redux-saga/internal/sagaHelpers' {
  type Predicate = (action: any) => boolean;
  type Pattern = string | string[] | Predicate;

  export function takeEvery(pattern: Pattern, worker, args?);
  export function takeLatest(pattern: Pattern, worker, args?);
}
