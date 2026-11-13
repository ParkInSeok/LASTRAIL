import {adminApi} from '../../api/client';
export function cosmeticReplacementPort(){return ()=>adminApi.eventCosmeticReplacementTable();}
