import {afterEach,describe,it,expect,vi} from "vitest";
import {renderToStaticMarkup} from "react-dom/server";
import {parseProbabilitySource,probabilityTargetSchema} from "./probabilityDisclosure";
import {ProbabilitySourceView} from "../../pages/ProbabilityDisclosuresPage";
import {adminApi} from "../api/client";
const target={consumer:"OFFLINE" as const,sourceKind:"AD_POLICY" as const,policyVersion:"v1"};
const source={environment:"DEV",consumer:"OFFLINE",policyVersion:"v1",scope:"MULTIPLIER_DISTRIBUTION",sourceDigest:'sha256:'+"a".repeat(64),distribution:[{multiplier:2,numerator:0,denominator:10000,percentage:"0.00"},{multiplier:3,numerator:1,denominator:10000,percentage:"0.01"},{multiplier:4,numerator:9999,denominator:10000,percentage:"99.99"}],contractVersion:"admin-probability-source-v1",disclosureId:'pd_'+"b".repeat(64),publicationStatus:"UNPUBLISHED",canPublish:true,publicSchemaVersion:1,sourceStatus:"ACTIVE"};
describe("probability disclosure read boundary",()=>{
 afterEach(()=>vi.unstubAllGlobals());
 it("renders exact precision and zero-weight rows",()=>{const html=renderToStaticMarkup(<ProbabilitySourceView source={parseProbabilitySource(source,"DEV",target)}/>);for(const text of ["0.00","0.01","99.99","2","3","4","미게시"])expect(html).toContain(text);});
 it.each([{environment:"PRD"},{consumer:"COMBAT"},{policyVersion:"v2"},{scope:"ALL_PROBABILITIES"},{sourceDigest:"bad"},{disclosureId:"pd_bad"},{sourceStatus:"CONFIGURED"},{publicationStatus:"PUBLISHED"},{distribution:[...source.distribution.slice(0,2),{...source.distribution[2],percentage:"100.00"}]},{distribution:[...source.distribution.slice(0,2),{...source.distribution[2],numerator:9998,percentage:"99.98"}]}])("rejects mismatched or inconsistent source %j",delta=>expect(()=>parseProbabilitySource({...source,...delta},"DEV",target)).toThrow());
 it("distinguishes purchase and ad schema instead of mixing sources",()=>{const purchase={...source,publicSchemaVersion:2,sourceKind:"PURCHASE_POLICY",sourceStatus:"CONFIGURED"};expect(()=>parseProbabilitySource(purchase,"DEV",target)).toThrow();expect(parseProbabilitySource(purchase,"DEV",{...target,sourceKind:"PURCHASE_POLICY"}).publicSchemaVersion).toBe(2);});
 it("retired policy remains inspectable but cannot be publishable",()=>{expect(parseProbabilitySource({...source,sourceStatus:"RETIRED",canPublish:false},"DEV",target).sourceStatus).toBe("RETIRED");expect(()=>parseProbabilitySource({...source,sourceStatus:"RETIRED"},"DEV",target)).toThrow();});
 it("validates exact version and performs only a read",async()=>{expect(probabilityTargetSchema.safeParse({...target,policyVersion:""}).success).toBe(false);const fetcher=vi.fn(async()=>Response.json(source));vi.stubGlobal("fetch",fetcher);await adminApi.probabilitySource("DEV",target);const [url,init]=fetcher.mock.calls[0] as unknown as [string,RequestInit];expect(url).toContain("consumer=OFFLINE&sourceKind=AD_POLICY&policyVersion=v1");expect(init.method).toBeUndefined();});
});
