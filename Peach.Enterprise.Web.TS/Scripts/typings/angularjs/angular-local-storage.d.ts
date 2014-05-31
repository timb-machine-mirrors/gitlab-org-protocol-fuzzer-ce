// Type definitions for Angular Local Storage


/// <reference path="../jquery/jquery.d.ts" />


declare module ng {
	export interface ILocalStorageService {
		isSupported: boolean;
		getStorageType(): string;
		set(key: string, value: any): boolean;
		add(key: string, value: any): boolean;
		get(key: string): any;
		keys(): string[];
		remove(key: string): boolean;
		clearAll(): boolean;
		cookie: ng.ICookieStorageService;
	}

	export interface ICookieStorageService {
		set(key: string, value: any): boolean;
		add(key: string, value: any): boolean;
		get(key: string): any;
		remove(key: string): boolean;
		clearAll(): boolean;
	}
}
