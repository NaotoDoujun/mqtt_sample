import React from 'react'
import clsx from 'clsx';
import {
  Card,
  CardHeader,
  CardContent
} from '@material-ui/core';
import { Theme, makeStyles, createStyles } from '@material-ui/core/styles'
import { LogChart } from './LogChart';

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      padding: theme.spacing(1)
    },
    cardheader: {
      backgroundColor: theme.palette.primary.main,
      color: theme.palette.primary.contrastText
    },
    cardcontent: {
      padding: theme.spacing(2),
      display: 'flex',
      overflow: 'auto',
      flexDirection: 'column',
    },
    fixedHeight: {
      height: 360,
    },
  }))
const Graph: React.FC<any> = (props: any) => {
  const classes = useStyles()
  const fixedHeight = clsx(classes.cardcontent, classes.fixedHeight);
  return (
    <Card className={classes.root}>
      <CardHeader title="Graph" className={classes.cardheader} />
      <CardContent className={fixedHeight}>
        <LogChart />
      </CardContent>

    </Card>
  )
}

export { Graph }