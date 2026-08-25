const ENCODING = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

function encodeCharacter(index: number): string {
  const character = ENCODING[index];
  if (character === undefined) {
    throw new Error("ULID_ENCODING_INDEX_OUT_OF_RANGE");
  }
  return character;
}

function encodeTime(time: number): string {
  let value = time;
  let output = "";
  for (let index = 0; index < 10; index += 1) {
    output = encodeCharacter(value % 32) + output;
    value = Math.floor(value / 32);
  }
  return output;
}

function encodeRandom(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  let output = "";
  for (const byte of bytes) {
    output += encodeCharacter(byte % 32);
  }
  return output;
}

export function createRequestId(): string {
  return `req_${encodeTime(Date.now())}${encodeRandom()}`;
}
