import React from 'react'
import { useApolloClient, gql } from '@apollo/client'

const Sub: React.FC<any> = (props: any) => {
  const client = useApolloClient()
  const data = client.readQuery({
    query: gql`
        query GetCounters {
          counters {
            id
            nodeId
            count
            recordTime
          }
        }
      `,
  })
  if (!data) return <></>
  const latest = data.counters.slice(-1)[0]

  const convUTC2JST = (value: string): string => {
    const date = new Date(value);
    var str = date.getFullYear()
      + '/' + ('0' + (date.getMonth() + 1)).slice(-2)
      + '/' + ('0' + date.getDate()).slice(-2)
      + ' ' + ('0' + date.getHours()).slice(-2)
      + ':' + ('0' + date.getMinutes()).slice(-2)
      + ':' + ('0' + date.getSeconds()).slice(-2)
      + '.' + ('00' + date.getMilliseconds()).slice(-3)
      + ' (JST)';
    return str;
  }

  return (
    <div>
      <h5>Sub Page using cache</h5>
      <p>id is {latest.id}, latest count is {latest.count}</p>
      <p>recorded at {convUTC2JST(latest.recordTime)}</p>
    </div>
  );
}

export { Sub }