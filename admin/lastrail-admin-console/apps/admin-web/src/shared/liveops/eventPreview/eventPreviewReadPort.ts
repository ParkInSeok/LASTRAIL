import {adminApi} from "../../api/client";
import {EventPreviewModel,type EventPreviewScope} from "./eventPreviewModel";
/** Host above transient Views; dispose and replace when this authenticated scope changes. */
export function createEventPreviewModel(scope:EventPreviewScope){return new EventPreviewModel(scope,input=>adminApi.eventDefinitionPreview(input));}
