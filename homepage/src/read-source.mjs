// Replace this adapter only after approved public routes and schema are agreed.
// Missing data is not a successful empty production response.
export class UnconnectedPublicSource {
  read() { return Object.freeze({kind:'unconnected',status:null,notices:[],catalog:[],disclosures:[],posts:[]}); }
}
