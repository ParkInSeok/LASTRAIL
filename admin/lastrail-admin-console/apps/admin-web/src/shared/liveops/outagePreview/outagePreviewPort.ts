import {adminApi} from '../../api/client';import type {OutagePreviewInput} from './outagePreviewContract';
export function outagePreviewPort(){return (input:OutagePreviewInput)=>adminApi.eventOutagePreview(input);}
