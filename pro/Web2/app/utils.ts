import moment = require('moment');
import { Dispatch } from 'redux';
import { FieldProp } from 'redux-form';

export function MakeEnum(obj: any) {
	Object.keys(obj).map(key => obj[key] = key);
}

export function MakeLowerEnum(obj: any) {
	Object.keys(obj).map(key => obj[key] = key.toLowerCase());
}

export function formatDate(date: Date | string): string {
	if (!date)
		return '---';
	return moment(date).format('M/D/YY h:mm a');
}

export function formatFileSize(bytes: number, precision: number = 1): string {
	const units = [
		'bytes',
		'KB',
		'MB',
		'GB',
		'TB',
		'PB'
	];

	if (bytes === 0) {
		return '0 bytes';
	}

	let unit = 0;

	while (bytes >= 1024) {
		bytes /= 1024;
		unit++;
	}

	const value = bytes.toFixed(precision);
	return (value.match(/\.0*$/) ? value.substr(0, value.indexOf('.')) : value) + ' ' + units[unit];
}

export function validationState(field: FieldProp) {
	if (field.error)
		return 'error';
}

export const validationMessages = {
	required: 'Required',
	unique: 'A unique value must be provided',
	integer: 'Not an integer',
	number: 'Not a number',
	rangeMin: 'Value is less than minimum allowed',
	rangeMax: 'Value is greater than maximum allowed',
	editable: 'Invalid value'
};
