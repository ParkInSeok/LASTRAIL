import {it,expect} from 'vitest';
import {managementRoute} from './managementRoutes';
it('accepts exact draft routes with original keys and readonly calculation/table',()=>{
 for(const path of ['/events/definitions/round%20one/commands','/reward-tracking/drafts/batch%20one/commands'])expect(managementRoute(path,'POST',new URLSearchParams(),false)).toEqual({requiresKey:true});
 for(const path of ['/events/definitions/round','/reward-tracking/drafts/batch','/events/cosmetic-replacement-table'])expect(managementRoute(path,'GET',new URLSearchParams(),false)).toEqual({requiresKey:false});
 expect(managementRoute('/events/definitions','GET',new URLSearchParams('after=round&limit=20'),false)).toEqual({requiresKey:false});
 expect(managementRoute('/events/outage-preview','POST',new URLSearchParams(),false)).toEqual({requiresKey:false});
 for(const path of ['/events/definitions/round/publish','/reward-tracking/drafts/batch/pay','/events/definitions/round/commands'])expect(managementRoute(path,'GET',new URLSearchParams(),false)?.error).toBe('contract_violation');
});
