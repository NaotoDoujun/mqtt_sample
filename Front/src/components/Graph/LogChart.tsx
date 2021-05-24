import React from 'react'
import { useQuery } from '@apollo/client'
import { CircularProgress } from '@material-ui/core'
import { useTheme } from '@material-ui/core/styles'
import { LineChart, Line, XAxis, YAxis, Label, ResponsiveContainer } from 'recharts'
import { Log, Chart, LogsForChart, LOGCHART_QUERY } from '../Types'

const LogChart: React.FC<any> = (props: any) => {
  const theme = useTheme();
  const { loading, error, data, startPolling, stopPolling } = useQuery<LogsForChart>(LOGCHART_QUERY)

  React.useEffect(() => {
    startPolling(5000)
    return () => {
      stopPolling()
    }
  }, [startPolling, stopPolling])

  if (error) <p>Got Error...</p>

  const convChartData = (logs: Log[]): Chart[] => {
    return logs.map(l => {
      const rt = new Date(l.recordTime)
      const recordTime = ('0' + rt.getHours()).slice(-2) + ':' + ('0' + rt.getMinutes()).slice(-2)
      return { time: recordTime, value: l.count }
    })
  }

  return (
    <>
      {loading ? <CircularProgress /> :
        <ResponsiveContainer>
          <LineChart
            data={data ? convChartData(data.logsForChart) : []}
            margin={{
              top: 16,
              right: 16,
              bottom: 0,
              left: 24,
            }}
          >
            <XAxis dataKey="time" stroke={theme.palette.text.secondary} />
            <YAxis stroke={theme.palette.text.secondary}>
              <Label
                angle={270}
                position="left"
                style={{ textAnchor: 'middle', fill: theme.palette.text.primary }}
              >
                Count
              </Label>
            </YAxis>
            <Line isAnimationActive={false}
              type="monotone"
              dataKey="value"
              stroke={theme.palette.primary.main}
              dot={false} />
          </LineChart>
        </ResponsiveContainer>
      }
    </>
  )
}

export { LogChart }