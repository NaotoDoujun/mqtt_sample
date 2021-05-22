import React from 'react'
import {
  Card,
  CardHeader,
  Divider
} from '@material-ui/core';
import { Theme, makeStyles, createStyles } from '@material-ui/core/styles'
import { Count } from './Count'
const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      padding: theme.spacing(1)
    },
    cardheader: {
      backgroundColor: theme.palette.primary.main,
      color: theme.palette.primary.contrastText
    },
  }))
const Dashboard: React.FC<any> = (props: any) => {
  const classes = useStyles()
  return (
    <Card className={classes.root}>
      <CardHeader title="Dashboard" className={classes.cardheader} />
      <Divider />
      <Count />
    </Card>
  )
}

export { Dashboard }