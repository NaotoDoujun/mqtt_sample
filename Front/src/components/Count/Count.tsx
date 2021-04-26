import React from 'react'
import { useQuery, gql } from '@apollo/client'
import { CircularProgress, Button } from '@material-ui/core'
import { Link } from 'react-router-dom'

interface Counter {
  id: number
  nodeId: string
  count: number
  recordTime: string
}

const COUNT_QUERY = gql`
  query Counters {
    counters {
      id
      nodeId
      count
      recordTime
    }
  }
`;

const COUNT_SUBSCRIPTION = gql`
  subscription OnRecorded {
    onRecorded {
      id
      nodeId
      count
      recordTime
    }
  }
`;

const Count: React.FC<any> = (props: any) => {
  const { loading, error, data, subscribeToMore } = useQuery(COUNT_QUERY)

  React.useEffect(
    () => subscribeToMore({
      document: COUNT_SUBSCRIPTION,
      updateQuery: (prev, { subscriptionData }) => {
        if (!subscriptionData.data) return prev
        const newCount = subscriptionData.data.onRecorded
        return Object.assign({}, prev, {
          counters: [...prev.counters, newCount],
        })
      },
    }),
    [subscribeToMore]
  )

  if (loading) return <CircularProgress />
  if (error) return <p>Got Error...</p>

  const latest = data.counters.slice(-1)[0] as Counter

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
      <h5>Counter</h5>
      <p>id is {latest.id}, latest count is {latest.count}</p>
      <p>recorded at {convUTC2JST(latest.recordTime)}</p>
      <Button variant="contained" component={Link} to="sub">
        Sub
      </Button>
    </div>
  );
}

export { Count }