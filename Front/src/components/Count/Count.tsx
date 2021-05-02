import React from 'react'
import { useQuery, gql } from '@apollo/client'
import {
  Paper,
  CircularProgress,
  TableContainer,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow
} from '@material-ui/core';

interface Counter {
  nodeId: string
  count: number
  recordTime: string
}

const COUNT_QUERY = gql`
  query Counters {
    latests {
      nodeId
      count
      recordTime
    }
  }
`;

const COUNT_SUBSCRIPTION = gql`
  subscription OnRecorded {
    onRecorded {
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
        const newCount = subscriptionData.data.onRecorded;
        const prevCounts = [...prev.latests].filter(c => c.nodeId !== newCount.nodeId)
        return Object.assign({}, prev, {
          latests: [newCount, ...prevCounts],
        })
      },
    }),
    [subscribeToMore]
  )

  if (loading) return <CircularProgress />
  if (error) return <p>Got Error...</p>

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
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>NodeId</TableCell>
              <TableCell>Count</TableCell>
              <TableCell>RecordTime</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data.latests.map((counter: Counter, index: number) =>
              <TableRow key={index}>
                <TableCell>{counter.nodeId}</TableCell>
                <TableCell>{counter.count}</TableCell>
                <TableCell>{convUTC2JST(counter.recordTime)}</TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </div>
  );
}

export { Count }