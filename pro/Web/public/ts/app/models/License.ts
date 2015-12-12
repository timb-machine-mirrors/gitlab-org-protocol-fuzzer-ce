/// <reference path="../reference.ts" />

namespace Peach {
	export interface ILicense {
		isValid: boolean;
		isInvalid: boolean;
		isMissing: boolean;
		isExpired: boolean;
		errorText: string;
		expiration: string;
		version: string;
	}
}
 