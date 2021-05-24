
export const convUTC2JST = (value: string): string => {
  const date = new Date(value);
  let str = date.getFullYear()
    + '/' + ('0' + (date.getMonth() + 1)).slice(-2)
    + '/' + ('0' + date.getDate()).slice(-2)
    + ' ' + ('0' + date.getHours()).slice(-2)
    + ':' + ('0' + date.getMinutes()).slice(-2)
    + ':' + ('0' + date.getSeconds()).slice(-2)
    + '.' + ('00' + date.getMilliseconds()).slice(-3)
    + ' (JST)';
  return str;
}